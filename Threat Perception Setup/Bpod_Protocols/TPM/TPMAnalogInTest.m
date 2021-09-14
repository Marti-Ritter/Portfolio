function TPM
global BpodSystem
global PCSocket
global CurrentTrial

%% Setup (runs once before the first trial and stops the code until user continues)
MaxTrials = 1000; % Set to some sane value, for preallocation

%--- Define parameters and trial structure
S = BpodSystem.ProtocolSettings; % Load settings chosen in launch manager into current workspace as a struct called S
if isempty(fieldnames(S))  % If settings file was an empty struct, populate struct with default settings
    S.GUI.TrialLength = 10; % how long does the trial last
    S.GUI.LickWait = 2; % how much time has the mouse to lick on the spout
    S.GUI.RewardWait = 2; % how much time has the mouse to consume the reward
    S.GUIPanels.Timing = {'TrialLength', 'LickWait', 'RewardWait'}; % UI panel for timing parameters

    S.GUI.AutoReward = 1; % Whether the mouse will be automatically rewarded (pairing).
    S.GUIMeta.AutoReward.Style = 'checkbox'; % Turn the previous setting into a checkbox
    S.GUI.RewardAmount = 20; % In ul.
    S.GUI.RewardDuration = 0.1; % In s. Only gets updated by the above value and, since there is no function to get reward from the valvetime, wont change anything if altered manually.
    S.GUIPanels.Reward = {'AutoReward', 'RewardAmount', 'RewardDuration'}; % UI panel for reward parameters

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

AnalogIn = BpodAnalogIn('COM7');
AnalogIn.nActiveChannels = 4;
AnalogIn.Thresholds = [10,10,4,3,10,10,10,10];
AnalogIn.ResetVoltages = [-10,-10,1,1,-10,-10,-10,-10];
AnalogIn.SMeventsEnabled = [0,0,1,1,0,0,0,0];
AnalogIn.SamplingRate = 10000;
AnalogIn.scope();
BpodSystem.Data.Traces = {};
disp('Analog Input Module set up.')

PCSocket = tcpip('localhost', 30000, 'NetworkRole', 'server', 'Timeout', inf);
system('D:\Users\TPM\Anaconda3\envs\TPM\pythonw.exe D:\Users\TPM\PycharmProjects\TPM\PC\PC_Recording.py &');
fopen(PCSocket);
fread(PCSocket,1);
disp('Recorders connected, confirmation byte received.')

%% Main trial loop
for CurrentTrial = 1:MaxTrials    
    S = BpodParameterGUI('sync', S); % Sync parameters with BpodParameterGUI plugin
    %R = GetValveTimes(S.GUI.RewardAmount, [1]); ValveTime = R(1); % Update reward amount
    %S.GUI.RewardDuration = ValveTime;

    SoftCodeHandler('SetLocation');

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
    
    switch S.GUI.AutoReward % Decide whether reward will be dispensed automatically after the AutoRewardPeriod or the mouse has to lick in the ResponsePeriod
        case 0
            ResponsePeriod = 'S2';
        case 1
            ResponsePeriod = 'AutoReward';
    end

    %--- Writing the disk state to the Raspberry Pi
    ModuleWrite('RaspbPi1', TrialTypes(CurrentTrial) - 1 + 30);

    sma = NewStateMachine(); % Initialize new state machine description
    sma = SetGlobalTimer(sma, 'TimerID', 1,... 
            'Duration', 0.005, 'OnsetDelay', 0,...
            'Channel', 'BNC1', 'OnsetValue', 1,... 
            'OffsetValue', 0, 'Loop', 1,...
            'GlobalTimerEvents', 1, 'LoopInterval', 0.005);
    sma = AddState(sma, 'Name', 'S0', ...
        'Timer', 0.05,...
        'StateChangeConditions', {'Tup', 'S1'},...
        'OutputActions', {'SoftCode', 1, 'RaspbPi1', 1, 'GlobalTimerTrig', 1}); 
    sma = AddState(sma, 'Name', 'S1', ...
        'Timer', S.GUI.TrialLength,...
        'StateChangeConditions', {'RaspbPi1_1', ResponsePeriod, 'Tup', 'S6'},...
        'OutputActions', {}); 
    sma = AddState(sma, 'Name', 'S2', ...
        'Timer', S.GUI.LickWait,...
        'StateChangeConditions', {'Port1In', 'S3', 'Tup', 'S7', 'RaspbPi1_2', 'S8'},...
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
        'Timer', 0,...
        'StateChangeConditions', {'Tup', 'exit'},...
        'OutputActions', {'SoftCode', 2, 'RaspbPi1', 2, 'GlobalTimerCancel', 1});
    sma = AddState(sma, 'Name', 'S6', ...
        'Timer', 0,...
        'StateChangeConditions', {'Tup', 'exit'},...
        'OutputActions', {'SoftCode', 2, 'RaspbPi1', 2, 'GlobalTimerCancel', 1});
    sma = AddState(sma, 'Name', 'S7', ...
        'Timer', 0,...
        'StateChangeConditions', {'Tup', 'exit'},...
        'OutputActions', {'SoftCode', 2, 'RaspbPi1', 2, 'GlobalTimerCancel', 1}); 
    sma = AddState(sma, 'Name', 'S8', ...
        'Timer', 0,...
        'StateChangeConditions', {'Tup', 'exit'},...
        'OutputActions', {'SoftCode', 2, 'RaspbPi1', 2, 'GlobalTimerCancel', 1}); 

    SendStateMatrix(sma); % Send state machine to the Bpod state machine device
    RawEvents = RunStateMatrix; % Run the trial and return events
    
    %--- Tell the recorders to save their files
    SoftCodeHandler(3);
    
    % Receive the Raspi dict.
    RaspiSocket = tcpclient("169.254.233.213",40000);
    if BpodSystem.Status.BeingUsed == 0
        ModuleWrite('RaspbPi1', 2)
    end
    while RaspiSocket.BytesAvailable == 0
        pause(0.01)
    end
    RaspiData = [];
    while RaspiSocket.BytesAvailable > 0
        RaspiData = [RaspiData, read(RaspiSocket)];
    end
    if ~isempty(RaspiData)
        while true
            try
                RaspiStruct = jsondecode(native2unicode(RaspiData, 'utf-8'));
                break;
            catch
                if RaspiSocket.BytesAvailable > 0
                    RaspiData = [RaspiData, read(RaspiSocket)];
                else
                    disp('No data available from Raspberry Pi, but JSON incomplete.');
                end
            end
        end
    end
    clear RaspiSocket;
        
    %--- Package and save the trial's data, update plots
    if ~isempty(fieldnames(RawEvents)) % If trial data was returned
        BpodSystem.Data = AddTrialEvents(BpodSystem.Data,RawEvents); % Computes trial events from raw data
        BpodSystem.Data = BpodNotebook('sync', BpodSystem.Data); % Sync with Bpod notebook plugin
        BpodSystem.Data.TrialSettings(CurrentTrial) = S; % Adds the settings used for the current trial to the Data struct (to be saved after the trial ends)
        BpodSystem.Data.TrialTypes(CurrentTrial) = TrialTypes(CurrentTrial); % Adds the trial type of the current trial to data
        lv1 = fieldnames(RaspiStruct);
        for n1=1:numel(lv1)
            BpodSystem.Data.RaspbPiRecords(CurrentTrial).(lv1{n1}) = RaspiStruct.(lv1{n1});
        end
        SaveBpodSessionData; % Saves the field BpodSystem.Data to the current data file
        
        %--- Typically a block of code here will update online plots using the newly updated BpodSystem.Data
        Outcomes = zeros(1,BpodSystem.Data.nTrials);
        for x = 1:BpodSystem.Data.nTrials
            if ~isnan(BpodSystem.Data.RawEvents.Trial{x}.States.S5(1))
                Outcomes(x) = 1;
            elseif ~isnan(BpodSystem.Data.RawEvents.Trial{x}.States.S7(1))
                Outcomes(x) = 2;
            elseif ~isnan(BpodSystem.Data.RawEvents.Trial{x}.States.S8(1))
                Outcomes(x) = -1;
            elseif ~isnan(BpodSystem.Data.RawEvents.Trial{x}.States.S6(1))
                Outcomes(x) = 3;
            end
        end
    % TrialTypeOutcomePlot(BpodSystem.GUIHandles.OutcomePlot,'update',BpodSystem.Data.nTrials+1,TrialTypes,Outcomes);
    end
    
    disp('finished matlab saving.')
    
    % Wait for the confirmation byte that the saving was finished.
    fread(PCSocket,1);
    
    disp('finished recordings saving.')
    
    % Wait for the confirmation byte that the tube has reset.
    RaspiSocket = tcpclient("169.254.233.213",50000);
    while RaspiSocket.BytesAvailable == 0
        pause(0.01)
    end
    read(RaspiSocket, 1); % read a single confirmation-byte
    
    disp('finished tube reset.')
    
    %--- This final block of code is necessary for the Bpod console's pause and stop buttons to work
    HandlePauseCondition; % Checks to see if the protocol is paused. If so, waits until user resumes.
    if BpodSystem.Status.BeingUsed == 0
        CleanUp
        return
    end
end
%--- Here we clear all remaining elements before the protocol ends
CleanUp
end

function CleanUp
    global PCSocket
    SoftCodeHandler(4);
    disp('Python processes have shut down.');
    % Wait for the confirmation byte that the shutting down was finished.
    fread(PCSocket,1);
    clear PCSocket
    
    ModuleWrite('RaspbPi1', 5);
    disp('Raspberry Pi told to shut down screen.');
end
