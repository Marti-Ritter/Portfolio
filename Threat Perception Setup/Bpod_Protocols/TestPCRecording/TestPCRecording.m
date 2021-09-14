function TestPCRecording 
global BpodSystem
global PCSocket
global CurrentTrial

%% Setup (runs once before the first trial)
MaxTrials = 10000; % Set to some sane value, for preallocation

%--- Define parameters and trial structure
S = BpodSystem.ProtocolSettings; % Loads settings file chosen in launch manager into current workspace as a struct called 'S'
if isempty(fieldnames(S))  % If chosen settings file was an empty struct, populate struct with default settings
    % Define default settings here as fields of S (i.e S.InitialDelay = 3.2)
    % Note: Any parameters in S.GUI will be shown in UI edit boxes. 
    % See ParameterGUI plugin documentation to show parameters as other UI types (listboxes, checkboxes, buttons, text)
    S.GUI.TrialLength = 10; % how long does the trial last
    S.GUI.LickWait = 2; % how much time has the mouse to lick on the spout
    S.GUI.RewardWait = 2; % how much time has the mouse to consume the reward
    S.GUIPanels.Timing = {'TrialLength', 'LickWait', 'RewardWait'}; % UI panel for timing parameters

end

%--- Initialize plots and start USB connections to any modules
BpodParameterGUI('init', S); % Initialize parameter GUI plugin

%% Define serial messages being sent to modules
BpodSystem.SoftCodeHandlerFunction = 'SoftCodeHandler';

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
PCSocket = tcpip('localhost', 30000, 'NetworkRole', 'server', 'Timeout', inf);
% system('D:\Users\TPM\Anaconda3\envs\TPM\pythonw.exe D:\Users\TPM\PycharmProjects\TPM\PC\PC_Recording.py &');
fopen(PCSocket);
fread(PCSocket,1);
disp('Recorders connected, confirmation byte received.')

%% Main loop (runs once per trial)
for CurrentTrial = 1:MaxTrials
    S = BpodParameterGUI('sync', S); % Sync parameters with BpodParameterGUI plugin
    
    %--- Typically, a block of code here will compute variables for assembling this trial's state machine
    
    SoftCodeHandler('SetLocation');

    %--- Assemble state machine
    sma = NewStateMachine();
    sma = SetGlobalTimer(sma, 'TimerID', 1,... 
        'Duration', 0.005, 'OnsetDelay', 0,...
        'Channel', 'BNC1', 'OnsetValue', 1,... 
        'OffsetValue', 0, 'Loop', 1,...
        'GlobalTimerEvents', 1, 'LoopInterval', 0.005);
    sma = AddState(sma, 'Name', 'State1', ... % This example state does nothing, and ends after 0 seconds
        'Timer', 3,...
        'StateChangeConditions', {'Tup', 'State2'},...
        'OutputActions', {'SoftCode', 1, 'GlobalTimerTrig', 1}); 
    sma = AddState(sma, 'Name', 'State2', ... % This example state does nothing, and ends after 0 seconds
        'Timer', 0,...
        'StateChangeConditions', {'Tup', 'State3'},...
        'OutputActions', {'SoftCode', 2, 'GlobalTimerCancel', 1}); 
    sma = AddState(sma, 'Name', 'State3', ... % This example state does nothing, and ends after 0 seconds
        'Timer', 2,...
        'StateChangeConditions', {'Tup', 'exit'},...
        'OutputActions', {}); 

    SendStateMatrix(sma); % Send state machine to the Bpod state machine device
    RawEvents = RunStateMatrix; % Run the trial and return events
    
    SoftCodeHandler(3);

    %--- Package and save the trial's data, update plots
    if ~isempty(fieldnames(RawEvents)) % If you didn't stop the session manually mid-trial
        BpodSystem.Data = AddTrialEvents(BpodSystem.Data,RawEvents); % Adds raw events to a human-readable data struct
        BpodSystem.Data.TrialSettings(CurrentTrial) = S; % Adds the settings used for the current trial to the Data struct (to be saved after the trial ends)
        SaveBpodSessionData; % Saves the field BpodSystem.Data to the current data file
        
        %--- Typically a block of code here will update online plots using the newly updated BpodSystem.Data
        
    end
    
    fread(PCSocket,1);
    disp('finished recordings saving.')

    
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
    % Wait for the confirmation byte that the shutting down was finished.
    fread(PCSocket,1);
    disp('Python processes have shut down.');
    clear PCSocket
end
