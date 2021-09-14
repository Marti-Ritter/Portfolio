function Test            
global BpodSystem

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
LoadSerialMessages('AnalogIn1', {['L' 1], ['L' 0]});  % Set serial messages 1+2 to start+stop logging
% AnalogIn1:
% command 1: start logging,
% command 2: stop logging

AnalogIn = BpodAnalogIn('COM7');
AnalogIn.nActiveChannels = 4;
AnalogIn.Thresholds = [10,10,4,3,10,10,10,10];
AnalogIn.ResetVoltages = [-10,-10,1,1,-10,-10,-10,-10];
AnalogIn.SMeventsEnabled = [0,0,1,1,0,0,0,0];
AnalogIn.SamplingRate = 10000;
BpodSystem.Data.Traces = {};
disp('Analog Input Module set up.')

%% Main loop (runs once per trial)
for currentTrial = 1:MaxTrials
    S = BpodParameterGUI('sync', S); % Sync parameters with BpodParameterGUI plugin
    
    %--- Typically, a block of code here will compute variables for assembling this trial's state machine
    AnalogIn.startReportingEvents();

    %--- Assemble state machine
    sma = NewStateMachine();
    sma = AddState(sma, 'Name', 'State1', ... % This example state does nothing, and ends after 0 seconds
        'Timer', 0,...
        'StateChangeConditions', {'Tup', 'State2'},...
        'OutputActions', {'AnalogIn1', 1}); 
    
    sma = AddState(sma, 'Name', 'State2', ... % This example state does nothing, and ends after 0 seconds
    'Timer', 1,...
    'StateChangeConditions', {'Tup', 'exit'},...
    'OutputActions', {'AnalogIn1', 2}); 

    SendStateMatrix(sma); % Send state machine to the Bpod state machine device
    RawEvents = RunStateMatrix; % Run the trial and return events
    
    AnalogIn.stopReportingEvents();

    TraceData = AnalogIn.getData()
    TraceData.y(1)

    %--- Package and save the trial's data, update plots
    if ~isempty(fieldnames(RawEvents)) % If you didn't stop the session manually mid-trial
        BpodSystem.Data = AddTrialEvents(BpodSystem.Data,RawEvents); % Adds raw events to a human-readable data struct
        BpodSystem.Data.TrialSettings(currentTrial) = S; % Adds the settings used for the current trial to the Data struct (to be saved after the trial ends)
        SaveBpodSessionData; % Saves the field BpodSystem.Data to the current data file
        
        %--- Typically a block of code here will update online plots using the newly updated BpodSystem.Data
        
    end
    
    %--- This final block of code is necessary for the Bpod console's pause and stop buttons to work
    HandlePauseCondition; % Checks to see if the protocol is paused. If so, waits until user resumes.
    if BpodSystem.Status.BeingUsed == 0
        return
    end
end