using MediatR;

namespace DocFlow.Application.Admin.Departamentos.Commands.EliminarDepartamento;

public record EliminarDepartamentoCommand(Guid Id) : IRequest;
