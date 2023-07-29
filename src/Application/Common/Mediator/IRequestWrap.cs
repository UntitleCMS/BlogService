﻿using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Mediator;
public interface IRequestWrap<T> : IRequest<IResponse<T>>
{
}
