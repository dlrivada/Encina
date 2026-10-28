```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|---------:|------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   873.29 ns |   3.030 ns | 2.686 ns |  1.09 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    13.47 ns |   0.045 ns | 0.042 ns |  0.02 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   789.45 ns |   1.777 ns | 1.576 ns |  0.99 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,476.61 ns |   5.087 ns | 4.759 ns |  1.85 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,484.67 ns |   6.238 ns | 5.835 ns |  1.86 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   797.67 ns |   2.316 ns | 2.053 ns |  1.00 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |          |       |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   876.82 ns |  26.900 ns | 1.474 ns |  1.09 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    13.51 ns |   0.687 ns | 0.038 ns |  0.02 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   802.84 ns |  55.659 ns | 3.051 ns |  1.00 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,542.34 ns | 100.934 ns | 5.533 ns |  1.92 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,463.38 ns |  46.719 ns | 2.561 ns |  1.82 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   803.11 ns |  19.376 ns | 1.062 ns |  1.00 | 0.0296 |     496 B |        1.00 |
