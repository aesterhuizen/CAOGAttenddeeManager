CREATE TABLE [dbo].[Attendance_Info] (
    [Attendance_InfoId] INT            IDENTITY (1, 1) NOT NULL,
    [Date]              DATETIME       NULL,
    [Status]            NVARCHAR (MAX) NULL,
    [AttendeeId]        INT            NULL,
	[Activity]			NVARCHAR (MAX) NULL,
	[Phone]				NVARCHAR (12)  NULL,
	[Email]				NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_dbo.Attendance_Info] PRIMARY KEY CLUSTERED ([Attendance_InfoId] ASC),
    CONSTRAINT [FK_dbo.Attendance_Info_dbo.Attendees_AttendeeId] FOREIGN KEY ([AttendeeId]) REFERENCES [dbo].[Attendees] ([AttendeeId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_AttendeeId]
    ON [dbo].[Attendance_Info]([AttendeeId] ASC);

