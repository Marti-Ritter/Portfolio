function TPM
global BpodSystem
global PCSocket
global CurrentTrial

%% Setup (runs once before the first trial and stops the code until user continues)
MaxTrials = 1000; % Set to some sane value, for preallocation

%--- Define parameters and trial structure
S = BpodSystem.ProtocolSettings; % Load settings chosen in launch manager into current workspace as a struct called S
if isempty(fieldnames(S))  % If settings file was an empty struct, populate struct with default settings
    S.GUI.TrialLength = 12; % how long does the trial last
    S.GUI.LickWait = 2; % how much time has the mouse to lick on the spout
    S.GUI.RewardWait = 2; % how much time has the mouse to consume the reward
    S.GUIPanels.Timing = {'TrialLength', 'LickWait', 'RewardWait'}; % UI panel for timing parameters

    S.GUI.AutoReward = 1; % Whether the mouse will be automatically rewarded (pairing).
    S.GUIMeta.AutoReward.Style = 'checkbox'; % Turn the previous setting into a checkbox
    S.GUI.NoTimeUp = 0; % Whether the mouse will be automatically rewarded (pairing).
    S.GUIMeta.NoTimeUp.Style = 'checkbox'; % Turn the previous setting into a checkbox
    S.GUI.RewardAmount = 20; % In ul.
    S.GUI.RewardDuration = 0.060; % In s. Only gets updated by the above value and, since there is no function to get reward from the valvetime, wont change anything if altered manually.
    S.GUIPanels.Reward = {'AutoReward', 'NoTimeUp', 'RewardAmount', 'RewardDuration'}; % UI panel for reward parameters

    TypeNames = {'Blocked', 'Smell', 'Visual', 'Open', 'Random'};
    S.GUI.TypeSelector = 1;
    S.GUIMeta.TypeSelector.Style = 'popupmenu';        % This dropdown-menu shows all available Types, which are contained in a list.
    S.GUIMeta.TypeSelector.String = TypeNames;       % Whatever is selected here will determine the TrialType to use for the next trial.
    S.GUI.BlockedProb = 0.25;
    S.GUI.SmellProb = 0.25;
    S.GUI.VisualProb = 0.25;
    S.GUI.OpenProb = 0.25;
    S.GUIPanels.TrialTypes = {'TypeSelector', 'BlockedProb', 'VisualProb', 'SmellProb', 'OpenProb'}; % UI panel for TrialType parameters
end

%% Define serial messages being sent to modules
LoadSerialMessages('AnalogIn1', {['L' 1], ['L' 0]});  % Set serial messages 1+2 to start+stop logging
% AnalogIn1:
% command 1: start logging,
% command 2: stop logging

%% Define serial messages being sent to modules
BpodSystem.SoftCodeHandlerFunction = 'SoftCodeHandler';

%% Define trials
% 1: Blocked, 2: Visual, 3: Smell, 4: Open
% Normalizing all probabilities, if someone enters a high number, and
% calculating the corresponding frequency
ProbList = [S.GUI.BlockedProb, S.GUI.VisualProb, S.GUI.SmellProb, S.GUI.OpenProb];
% Calculate the frequency of each state from the relative probability and
% the number of Trials, then calculate the raw TrialTypes vector by
% repeating the index of a trial type the required number of trials.
TrialTypes = [];
for i = 1:length(ProbList)
   Frequency = round((ProbList(i) / sum(ProbList)) * MaxTrials);
   TrialTypes = [TrialTypes, repmat([i], 1, Frequency)];
end
% If a single trialtype gets lost due to rounding, add a blocking state to
% the front.
if length(TrialTypes) < MaxTrials
    TrialTypes = [[1], TrialTypes];
end
% Shuffle everything
TrialTypes = TrialTypes(randperm(length(TrialTypes)));
BpodSystem.Data.TrialTypes = []; % The trial type of each trial completed will be added here.

OldProbs = ProbList; % Remembering this allows us to redraw the projected trials on the fly.

%% Initialize plots
BpodSystem.ProtocolFigures.OutcomePlotFig = figure('Position', [50 540 1000 250],'name','Outcome plot','numbertitle','off', 'MenuBar', 'none', 'Resize', 'off');
BpodSystem.GUIHandles.OutcomePlot = axes('Position', [.075 .3 .89 .6]);
TrialTypeOutcomePlot(BpodSystem.GUIHandles.OutcomePlot,'init',TrialTypes);

BpodNotebook('init'); % Initialize Bpod notebook (for manual data annotation)
BpodParameterGUI('init', S); % Initialize parameter GUI plugin

%% Set up Python environment
pyExec = 'D:\Users\TPM\Anaconda3\envs\TPM\pythonw.exe';
pyRoot = fileparts(pyExec);
p = getenv('PATH');
p = strsplit(p, ';');
addToPath = {
    pyRoot
    fullfile(pyRoot, 'Library', 'mingw-w64', 'bin')
    fullfile(pyRoot, 'Library', 'usr', 'bin')
    fullfile(pyRoot, 'Library', 'bin')
    fullfile(pyRoot, 'Scripts')
    fullfile(pyRoot, 'bin')
    };
p = [addToPath(:); p(:)];
p = unique(p, 'stable');
p = strjoin(p, ';');
setenv('PATH', p);

%% Prepare communication with other elements
ModuleWrite('RaspbPi1', 4);
BpodSystem.Data.RaspbPiRecords = {};
disp('Raspberry Pi told to start screen.')

AnalogIn = BpodAnalogIn('COM8');
AnalogIn.nActiveChannels = 4;
AnalogIn.Thresholds = [10,10,4,3,10,10,10,10];
AnalogIn.ResetVoltages = [-10,-10,1,1,-10,-10,-10,-10];
%AnalogIn.SMeventsEnabled = [0,0,1,1,0,0,0,0];
AnalogIn.SamplingRate = 1000;
BpodSystem.Data.Traces = {};
disp('Analog Input Module set up.')

PCSocket = tcpip('localhost', 30000, 'NetworkRole', 'server', 'Timeout', inf);
system('D:\Users\TPM\Anaconda3\envs\TPM\pythonw.exe D:\Users\TPM\PycharmProjects\TPM\PC\PC_Recording.py &');
fopen(PCSocket);
fread(PCSocket,1);
disp('Recorders connected, confirmation byte received.')

%% Main trial loop
tic;
for CurrentTrial = 1:MaxTrials    
    S = BpodParameterGUI('sync', S); % Sync parameters with BpodParameterGUI plugin
    %R = GetValveTimes(S.GUI.RewardAmount, [1]); ValveTime = R(1); % Update reward amount
    %S.GUI.RewardDuration = ValveTime;

    %--- Typically, a block of code here will compute variables for assembling this trial's state machine
    ProbList = [S.GUI.BlockedProb, S.GUI.VisualProb, S.GUI.SmellProb, S.GUI.OpenProb];
    if ~isequal(OldProbs, ProbList) % Check if someone changed the Probs
        OldProbs = ProbList;
        disp('Probs were adjusted.')
        NewTrialTypes = [];
        RemainingTrials = MaxTrials - CurrentTrial;
        for i=1:length(ProbList)
           Frequency = round((ProbList(i) / sum(ProbList)) * RemainingTrials);
           NewTrialTypes = [NewTrialTypes, repmat([i], 1, Frequency)];
        end
        NewTrialTypes = [NewTrialTypes, [1]];
        NewTrialTypes = NewTrialTypes(1:RemainingTrials);
        
        if length(NewTrialTypes) < RemainingTrials
            NewTrialTypes = [[1], NewTrialTypes];
        end
        disp(length(NewTrialTypes))
        disp(RemainingTrials)

        NewTrialTypes = NewTrialTypes(randperm(length(NewTrialTypes)));
        TrialTypes(CurrentTrial+1:MaxTrials) = NewTrialTypes;
    end
    if S.GUI.TypeSelector ~= 5
        TrialTypes(CurrentTrial) = S.GUI.TypeSelector;
    end
    if CurrentTrial ~= 1
        TrialTypeOutcomePlot(BpodSystem.GUIHandles.OutcomePlot,'update',BpodSystem.Data.nTrials+1,TrialTypes,Outcomes); % Update the Plot
    else
        TrialTypeOutcomePlot(BpodSystem.GUIHandles.OutcomePlot,'update',CurrentTrial,TrialTypes,[0]); % Update the Plot
    end
    % Decide whether reward will be dispensed automatically after the
    % AutoRewardPeriod or the mouse has to lick in the ResponsePeriod
    switch S.GUI.AutoReward 
        case 0
            ResponsePeriod = 'S2';
        case 1
            ResponsePeriod = 'AutoReward';
    end
    
    % Depending on whether NoTimeUp is activated, remove all recorder 
    % triggers to prevent a storage-overflow (no traces, no cameras or
    % microphones, and no Timers). The Raspberry Pi is triggered as usual.
    % Also, remove all TimeUp triggers, so that only input from the mouse
    % changes the states. In the case of AutoReward, there will be an
    % automatic dispense of reward as usual, with a TimeUp after half of
    % the LickWait duration.
    switch S.GUI.NoTimeUp
        case 0
            Recording = true;
            S_1OutputActions = {'AnalogIn1', 1};
            S0OutputActions = {'SoftCode', 1, 'RaspbPi1', 1, 'GlobalTimerTrig', 1};
            S1StateChangeConditions = {'RaspbPi1_1', ResponsePeriod, 'Tup', 'S6'};
            S2StateChangeConditions = {'Port1In', 'S3', 'Tup', 'S7', 'RaspbPi1_2', 'S8'};
            ExitOutputActions = {'SoftCode', 2, 'RaspbPi1', 2, 'GlobalTimerCancel', 1};
            S_2OutputActions = {'AnalogIn1', 2};
        case 1
            Recording = false;
            S_1OutputActions = {};
            S0OutputActions = {'RaspbPi1', 10};
            S1StateChangeConditions = {'RaspbPi1_1', ResponsePeriod};
            S2StateChangeConditions = {'Port1In', 'S3', 'RaspbPi1_2', 'S8'};
            ExitOutputActions = {'RaspbPi1', 2};
            S_2OutputActions = {};
    end

    if Recording
        SoftCodeHandler('SetLocation');
        %AnalogIn.startReportingEvents();
    end
    
    %--- Writing the disk state to the Raspberry Pi
    ModuleWrite('RaspbPi1', TrialTypes(CurrentTrial) - 1 + 30);

    sma = NewStateMachine(); % Initialize new state machine description
    sma = SetGlobalTimer(sma, 'TimerID', 1,... 
            'Duration', 0.005, 'OnsetDelay', 0,...
            'Channel', 'BNC1', 'OnsetValue', 1,... 
            'OffsetValue', 0, 'Loop', 1,...
            'GlobalTimerEvents', 0, 'LoopInterval', 0.005);
    sma = AddState(sma, 'Name', 'S_1', ...
        'Timer', 0.02,...
        'StateChangeConditions', {'Tup', 'S0'},...
        'OutputActions', S_1OutputActions); 
    sma = AddState(sma, 'Name', 'S0', ...
        'Timer', 0.1,...
        'StateChangeConditions', {'Tup', 'S1'},...
        'OutputActions', S0OutputActions); 
    sma = AddState(sma, 'Name', 'S1', ...
        'Timer', S.GUI.TrialLength,...
        'StateChangeConditions', S1StateChangeConditions,...
        'OutputActions', {}); 
    sma = AddState(sma, 'Name', 'S2', ...
        'Timer', S.GUI.LickWait,...
        'StateChangeConditions', S2StateChangeConditions,...
        'OutputActions', {});
    sma = AddState(sma, 'Name', 'AutoReward', ...
        'Timer', S.GUI.LickWait / 2,...
        'StateChangeConditions', {'Tup', 'S3', 'RaspbPi1_2', 'S8'},...
        'OutputActions', {});
    sma = AddState(sma, 'Name', 'S3', ...
        'Timer', S.GUI.RewardDuration,...
        'StateChangeConditions', {'Tup', 'S4'},...
        'OutputActions', {'Valve1', true});
    sma = AddState(sma, 'Name', 'S4', ...
        'Timer', S.GUI.RewardWait,...
        'StateChangeConditions', {'Tup', 'S5', 'RaspbPi1_2', 'S8'},...
        'OutputActions', {}); 
    sma = AddState(sma, 'Name', 'S5', ...
        'Timer', 0.02,...
        'StateChangeConditions', {'Tup', 'S_2'},...
        'OutputActions', ExitOutputActions);
    sma = AddState(sma, 'Name', 'S6', ...
        'Timer', 0.02,...
        'StateChangeConditions', {'Tup', 'S_2'},...
        'OutputActions', ExitOutputActions);
    sma = AddState(sma, 'Name', 'S7', ...
        'Timer', 0.02,...
        'StateChangeConditions', {'Tup', 'S_2'},...
        'OutputActions', ExitOutputActions); 
    sma = AddState(sma, 'Name', 'S8', ...
        'Timer', 0.02,...
        'StateChangeConditions', {'Tup', 'S_2'},...
        'OutputActions', ExitOutputActions); 
    sma = AddState(sma, 'Name', 'S_2', ...
        'Timer', 0,...
        'StateChangeConditions', {'Tup', 'exit'},...
        'OutputActions', S_2OutputActions); 

    fprintf("Running trial %i.\n", CurrentTrial)
    
    SendStateMatrix(sma); % Send state machine to the Bpod state machine device
    RawEvents = RunStateMatrix; % Run the trial and return events
        
    if BpodSystem.Status.BeingUsed == 0
        ModuleWrite('RaspbPi1', 2)
    end
    
    if Recording
        %--- Tell the recorders to save their files
        SoftCodeHandler(3);
    end

    % Wait for the confirmation byte that the dict is available.
    RaspiSocket = tcpclient("169.254.233.213",50000);
    while RaspiSocket.BytesAvailable == 0
        pause(0.01)
    end
    read(RaspiSocket, 1); % read a single confirmation-byte
    disp('Trial end byte received.');
    clear RaspiSocket;
    
    % Wait for the confirmation byte that the tube has reset.
    RaspiSocket = tcpclient("169.254.233.213",50000);
    while RaspiSocket.BytesAvailable == 0
        pause(0.01)
    end
    read(RaspiSocket, 1); % read a single confirmation-byte
    disp('finished tube reset.')
    clear RaspiSocket;
    
    if Recording
        %AnalogIn.stopReportingEvents();

        % Receive the Raspi dict.
        ModuleWrite('RaspbPi1', 20)
        disp('Requesting Raspi-dict.');

        RaspiSocket = tcpclient("169.254.233.213",40000);

        RaspiData = [];
        while RaspiSocket.BytesAvailable > 0
            RaspiData = [RaspiData, read(RaspiSocket)];
        end
        pause(0.1);
        while true
            try
                RaspiStruct = jsondecode(native2unicode(RaspiData, 'utf-8'));
                disp('Raspi-dict received.');
                break;
            catch
                if RaspiSocket.BytesAvailable > 0
                    RaspiData = [RaspiData, read(RaspiSocket)];
                else
                    disp('No data available from Raspberry Pi, but JSON incomplete.');
                end
            end
        end
        clear RaspiSocket;
    end
        
    %--- Package and save the trial's data, update plots
    if ~isempty(fieldnames(RawEvents)) % If trial data was returned
        BpodSystem.Data = AddTrialEvents(BpodSystem.Data,RawEvents); % Computes trial events from raw data
        BpodSystem.Data = BpodNotebook('sync', BpodSystem.Data); % Sync with Bpod notebook plugin
        BpodSystem.Data.TrialSettings(CurrentTrial) = S; % Adds the settings used for the current trial to the Data struct (to be saved after the trial ends)
        BpodSystem.Data.TrialTypes(CurrentTrial) = TrialTypes(CurrentTrial); % Adds the trial type of the current trial to data
        if Recording
            TraceData = AnalogIn.getData();
            BpodSystem.Data.Traces(CurrentTrial).x = TraceData.x;
            BpodSystem.Data.Traces(CurrentTrial).y = TraceData.y;
            lv1 = fieldnames(RaspiStruct);
            for n1=1:numel(lv1)
                BpodSystem.Data.RaspbPiRecords(CurrentTrial).(lv1{n1}) = RaspiStruct.(lv1{n1});
            end
        end
        SaveBpodSessionData; % Saves the field BpodSystem.Data to the current data file
        
        %--- Typically a block of code here will update online plots using the newly updated BpodSystem.Data
        Outcomes = zeros(1,BpodSystem.Data.nTrials);
        
        LatestTrialEnd = BpodSystem.Data.TrialEndTimestamp(end);
        SessionSuccessfulTrials = 0;
        FiveMinuteSuccessfulTrials = 0;
        FiveMinuteTotalTrials = 0;
        for x = 1:BpodSystem.Data.nTrials
            if ~isnan(BpodSystem.Data.RawEvents.Trial{x}.States.S5(1))
                Outcomes(x) = 1;
                SessionSuccessfulTrials = SessionSuccessfulTrials + 1;
                % Check if this successful trial happened in the past 5
                % minutes (300 seconds)
                if BpodSystem.Data.TrialStartTimestamp(x) > LatestTrialEnd - (5 * 60)
                    FiveMinuteSuccessfulTrials = FiveMinuteSuccessfulTrials + 1;
                end
            elseif ~isnan(BpodSystem.Data.RawEvents.Trial{x}.States.S7(1))
                Outcomes(x) = 2;
            elseif ~isnan(BpodSystem.Data.RawEvents.Trial{x}.States.S8(1))
                Outcomes(x) = -1;
            elseif ~isnan(BpodSystem.Data.RawEvents.Trial{x}.States.S6(1))
                Outcomes(x) = 3;
            end
            if BpodSystem.Data.TrialStartTimestamp(x) > LatestTrialEnd - (5 * 60)
                FiveMinuteTotalTrials = FiveMinuteTotalTrials + 1;
            end
        end
        tEnd = toc;
        disp('--------------------------------------');
        fprintf('Session length:             %02.0f:%02.0f\nSession success:         %4i/%4i (%6.1f%%)\nPast 5 minutes success:  %4i/%4i (%6.1f%%)\n',...
            floor(tEnd/60), rem(tEnd,60), ...
            SessionSuccessfulTrials, CurrentTrial, ...
            (SessionSuccessfulTrials / CurrentTrial * 100),...
            FiveMinuteSuccessfulTrials, FiveMinuteTotalTrials,...
            (FiveMinuteSuccessfulTrials / FiveMinuteTotalTrials * 100));
        disp('--------------------------------------');
    % TrialTypeOutcomePlot(BpodSystem.GUIHandles.OutcomePlot,'update',BpodSystem.Data.nTrials+1,TrialTypes,Outcomes);
    end
    
    disp('finished matlab saving.')
    
    if Recording
        % Wait for the confirmation byte that the saving was finished.
        fread(PCSocket,1);
        disp('finished recordings saving.')
    end
    
    fprintf("Trial %i finished.\n", CurrentTrial)
    
    %--- This final block of code is necessary for the Bpod console's pause and stop buttons to work
    HandlePauseCondition; % Checks to see if the protocol is paused. If so, waits until user resumes.
    if BpodSystem.Status.BeingUsed == 0
        CleanUp(Recording)
        return
    end
end
%--- Here we clear all remaining elements before the protocol ends
CleanUp(Recording)
end

function CleanUp(Recording)
    global PCSocket
    SoftCodeHandler(4);
    if Recording
        % Wait for the confirmation byte that the shutting down was finished.
        fread(PCSocket,1);
        disp('Python processes have shut down.');
    end
    clear PCSocket
    
    ModuleWrite('RaspbPi1', 5);
    disp('Raspberry Pi told to shut down screen.');
end
