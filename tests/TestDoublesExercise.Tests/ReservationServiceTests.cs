using Moq;

namespace TestDoublesExercise.Tests;

public sealed class ReservationServiceTests
{
    // Entfernt Skip jeweils erst, wenn ihr den Test implementiert habt.
    // Die Beispiele für Stub, Spy und Moq stehen in TestDoubleExamples.cs.

    [Fact]
    public void Reserve_WithEnoughSeats_ReturnsTrue()
    {
        // Arrange: Stub mit 3 freien Plätzen, Repository mit erfolgreichem Speichern,
        //          eigener Sender-Spy und ReservationService.
        var availability = new StubFixedSeatAvailability("TEST-1", 3);
        var repo = new RepositoryRecordingReservation(true);
        var sender = new SpyRecordingConfirmationSender();
        var service = new ReservationService(availability, repo, sender);
        
        // Act:     2 Plätze reservieren.
        var result = service.Reserve("user@ex.com", "TEST-1", 2);
        
        // Assert:  Ergebnis ist true.
        Assert.True(result);
        Assert.Single(repo.SavedReservations);
        Assert.Single(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotSaveOrSend()
    {
        // Arrange: Stub mit 1 freiem Platz und Doubles für Repository und Sender.
        var availability = new StubFixedSeatAvailability("TEST-2", 1);
        var repo = new RepositoryRecordingReservation();
        var sender = new SpyRecordingConfirmationSender();
        var service = new ReservationService(availability, repo, sender);
        
        // Act:     2 Plätze reservieren.
        var result = service.Reserve("user@ex.com", "TEST-2", 2);
        
        // Assert:  Ergebnis ist false; weder Speichern noch Senden wurde aufgerufen.
        Assert.False(result);
        Assert.Empty(repo.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithExactlyEnoughSeats_Succeeds()
    {
        // Arrange: Stub mit genau 2 freien Plätzen.
        var availability = new StubFixedSeatAvailability("TEST-3", 2);
        var repo = new RepositoryRecordingReservation();
        var sender = new SpyRecordingConfirmationSender();
        var service = new ReservationService(availability, repo, sender);
        
        // Act:     2 Plätze reservieren.
        var result = service.Reserve("user@ex.com", "TEST-3", 2);
        
        // Assert:  Ergebnis ist true.
        Assert.True(result);
        Assert.Single(repo.SavedReservations);
        Assert.Single(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithZeroSeats_ThrowsBeforeCallingDependencies()
    {
        // Arrange: Doubles, deren Aufrufe sich prüfen lassen.
        var availability = new StubFixedSeatAvailability("TEST-4", 3);
        var repo = new RepositoryRecordingReservation();
        var sender = new SpyRecordingConfirmationSender();
        var service = new ReservationService(availability, repo, sender);
        
        // Act:     0 Plätze reservieren.
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Reserve("user@ex.com", "TEST-4", 0));
        
        // Assert:  ArgumentOutOfRangeException; keine Abhängigkeit wurde aufgerufen.
        Assert.Equal(0, availability.CallCount);
        Assert.Empty(repo.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithNegativeSeats_ThrowsBeforeCallingDependencies()
    {
        // Arrange: Doubles, deren Aufrufe sich prüfen lassen.
        var availability = new StubFixedSeatAvailability("TEST-5", 3);
        var repo = new RepositoryRecordingReservation();
        var sender = new SpyRecordingConfirmationSender();
        var service = new ReservationService(availability, repo, sender);
        
        // Act:     -1 Plätze reservieren.
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Reserve("user@ex.com", "TEST-5", -1));
        
        // Assert:  ArgumentOutOfRangeException; keine Abhängigkeit wurde aufgerufen.
        Assert.Equal(0, availability.CallCount);
        Assert.Empty(repo.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithEmptyEventCode_ThrowsBeforeCallingDependencies()
    {
        // Arrange: Doubles, deren Aufrufe sich prüfen lassen.
        var availability = new StubFixedSeatAvailability("TEST-6", 3);
        var repo = new RepositoryRecordingReservation();
        var sender = new SpyRecordingConfirmationSender();
        var service = new ReservationService(availability, repo, sender);
        
        // Act:     Mit leerem Veranstaltungscode reservieren.
        Assert.Throws<ArgumentException>(() => service.Reserve("user@ex.com", "", 1));
        
        // Assert:  ArgumentException; keine Abhängigkeit wurde aufgerufen.
        Assert.Equal(0, availability.CallCount);
        Assert.Empty(repo.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithWhitespaceEmail_ThrowsBeforeCallingDependencies()
    {
        // Arrange: Handgeschriebene Doubles oder Moq gezielt auswählen.
        var availability = new Mock<ISeatAvailability>();
        var repo = new Mock<IReservationRepository>();
        var sender = new Mock<IConfirmationSender>();
        var service = new ReservationService(availability.Object, repo.Object, sender.Object);
        
        // Act:     Mit einer E-Mail-Adresse aus Leerzeichen reservieren.
        Assert.Throws<ArgumentException>(() => service.Reserve("   ", "TEST-7", 1));
        
        // Assert:  ArgumentException; keine Abhängigkeit wurde aufgerufen.
        availability.Verify(a => a.GetAvailableSeats(It.IsAny<string>()), Times.Never());
        repo.Verify(r => r.TrySave(It.IsAny<Reservation>()), Times.Never());
        sender.Verify(s => s.Send(It.IsAny<Reservation>()), Times.Never());
    }

    [Fact]
    public void Reserve_WhenSavingFails_DoesNotSendConfirmation()
    {
        // Arrange: Stub mit genügend Plätzen, Repository liefert false, eigener Spy.
        var availability = new StubFixedSeatAvailability("TEST-8", 3);
        var repo = new RepositoryRecordingReservation(false);
        var sender = new SpyRecordingConfirmationSender();
        var service = new ReservationService(availability, repo, sender);
        
        // Act:     Reservierung versuchen.
        var result = service.Reserve("user@ex.com", "TEST-8", 1);
        
        // Assert:  Ergebnis ist false und der Spy hat keinen Aufruf aufgezeichnet.
        Assert.False(result);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotCallRepository()
    {
        // Arrange: Stub mit zu wenigen Plätzen und Mock<IReservationRepository>.
        var availability = new StubFixedSeatAvailability("TEST-9", 1);
        var repo = new Mock<IReservationRepository>();
        var sender = new SpyRecordingConfirmationSender();
        var service = new ReservationService(availability, repo.Object, sender);
        
        // Act:     Reservierung versuchen.
        var result = service.Reserve("user@ex.com", "TEST-9", 2);
        
        // Assert:  Verify(..., Times.Never()) für TrySave.
        Assert.False(result);
        repo.Verify(r => r.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }

    [Fact(Skip = "Fortgeschrittene: ReserveBatch zuerst implementieren")]
    public void ReserveBatch_WhenCombinedSeatsExceedCapacity_DoesNotSaveOrSend()
    {
        // Arrange: Zwei Reservierungen für dieselbe Veranstaltung, die einzeln passen.
        // Act:     Nach Implementierung des Batch-Verfahrens beide zusammen reservieren.
        // Assert:  Ergebnis ist false; kein Batch-Speichern und keine Bestätigung.
        throw new NotImplementedException();
    }
    
    //> ------------------------- <//
    //>     HELPER FUNCTIONS      <//
    //> ------------------------- <//

    private sealed class StubFixedSeatAvailability(IReadOnlyDictionary<string, int> availableSeatsByEventCode) : ISeatAvailability
    {
        public StubFixedSeatAvailability(string eventCode, int availableSeats)
        : this( new Dictionary<string, int> { [eventCode] = availableSeats } )
        {
        }
        
        public int CallCount { get; private set; }

        public int GetAvailableSeats(string eventCode)
        {
            CallCount++;
            return availableSeatsByEventCode.GetValueOrDefault(eventCode, 0);
        }
    }

    private sealed class RepositoryRecordingReservation(bool trySaveResult = true) : IReservationRepository
    {
        public List<Reservation> SavedReservations { get; } = [];
        
        public bool TrySave(Reservation reservation)
        {
            SavedReservations.Add(reservation);
            return trySaveResult;
        }
    }

    private sealed class SpyRecordingConfirmationSender : IConfirmationSender
    {
        private readonly List<Reservation> _sentReservations = [];
        public IReadOnlyList<Reservation> SentReservations => _sentReservations;

        public void Send(Reservation reservation) => _sentReservations.Add(reservation);
    }
}
