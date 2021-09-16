function TestRaspberryPi 
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

%% Prepare communication with other elements
ModuleWrite('RaspbPi1', 4);
BpodSystem.Data.RaspbPiRecords = {};
disp('Raspberry Pi told to start screen.')

%% Main loop (runs once per trial)
for currentTrial = 1:MaxTrials
    S = BpodParameterGUI('sync', S); % Sync parameters with BpodParameterGUI plugin
    
    %--- Typically, a block of code here will compute variables for assembling this trial's state machine
    
    %--- Assemble state machine
    sma = NewStateMachine();
    sma = AddState(sma, 'Name', 'State1', ... % This example state does nothing, and ends after 0 seconds
        'Timer', 1,...
        'StateChangeConditions', {'Tup', 'State2'},...
        'OutputActions', {'RaspbPi1', 10}); 
    sma = AddState(sma, 'Name', 'State2', ... % This example state does nothing, and ends after 0 seconds
        'Timer', 0,...
        'StateChangeConditions', {'Tup', 'exit'},...
        'OutputActions', {'RaspbPi1', 2}); 

    SendStateMatrix(sma); % Send state machine to the Bpod state machine device
    RawEvents = RunStateMatrix; % Run the trial and return events
    
    if BpodSystem.Status.BeingUsed == 0
        ModuleWrite('RaspbPi1', 2)
    end

    %--- Package and save the trial's data, update plots
    if ~isempty(fieldnames(RawEvents)) % If you didn't stop the session manually mid-trial
        BpodSystem.Data = AddTrialEvents(BpodSystem.Data,RawEvents); % Adds raw events to a human-readable data struct
        BpodSystem.Data.TrialSettings(currentTrial) = S; % Adds the settings used for the current trial to the Data struct (to be saved after the trial ends)
        SaveBpodSessionData; % Saves the field BpodSystem.Data to the current data file
        
        %--- Typically a block of code here will update online plots using the newly updated BpodSystem.Data
        
    end
    
        % Wait for the confirmation byte that the tube has reset.
    RaspiSocket = tcpclient("169.254.233.213",50000);
    while RaspiSocket.BytesAvailable == 0
        disp('Waiting for reset')
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
    ModuleWrite('RaspbPi1', 5);
    disp('Raspberry Pi told to shut down screen.');
end
