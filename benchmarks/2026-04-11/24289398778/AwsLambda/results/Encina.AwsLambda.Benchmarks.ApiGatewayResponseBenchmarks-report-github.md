```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   752.55 ns |  3.249 ns |  3.039 ns |  1.15 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    13.77 ns |  0.054 ns |  0.045 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   623.30 ns |  2.165 ns |  2.026 ns |  0.95 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,439.48 ns |  5.557 ns |  4.926 ns |  2.19 |    0.01 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,391.23 ns |  4.483 ns |  4.193 ns |  2.12 |    0.01 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   657.11 ns |  3.982 ns |  3.530 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |           |       |         |        |           |             |
| ToCreatedResponse_Success    | MediumRun  | 15             | 2           | 10          |   710.39 ns |  2.937 ns |  4.020 ns |  1.09 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | MediumRun  | 15             | 2           | 10          |    13.77 ns |  0.047 ns |  0.069 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | MediumRun  | 15             | 2           | 10          |   633.42 ns |  2.260 ns |  3.168 ns |  0.97 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | MediumRun  | 15             | 2           | 10          | 1,432.23 ns | 15.222 ns | 21.830 ns |  2.20 |    0.04 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | MediumRun  | 15             | 2           | 10          | 1,469.79 ns | 11.437 ns | 16.764 ns |  2.26 |    0.03 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | MediumRun  | 15             | 2           | 10          |   651.52 ns |  4.519 ns |  6.481 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
