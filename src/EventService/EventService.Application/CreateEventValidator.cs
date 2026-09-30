using FluentValidation;

namespace EventService.Application;

public sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Date).Must(date => date > timeProvider.GetUtcNow()).WithMessage("La fecha debe estar en el futuro.");
        RuleFor(x => x.Venue).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Zones).NotEmpty().WithMessage("Debe registrar al menos una zona.");
        RuleForEach(x => x.Zones).ChildRules(zone =>
        {
            zone.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            zone.RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
            zone.RuleFor(x => x.Capacity).GreaterThan(0);
        });
    }
}
