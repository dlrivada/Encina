```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   858.12 ns |  1.184 ns |  1.050 ns |   857.96 ns |  1.11 |    0.00 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    13.47 ns |  0.033 ns |  0.031 ns |    13.48 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   793.96 ns |  0.771 ns |  0.644 ns |   794.13 ns |  1.03 |    0.00 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,431.83 ns |  2.906 ns |  2.718 ns | 1,431.92 ns |  1.86 |    0.01 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,472.47 ns |  3.306 ns |  3.093 ns | 1,471.65 ns |  1.91 |    0.01 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   769.65 ns |  1.693 ns |  1.584 ns |   769.06 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |           |             |       |         |        |           |             |
| ToCreatedResponse_Success    | MediumRun  | 15             | 2           | 10          |   863.34 ns |  1.719 ns |  2.572 ns |   864.01 ns |  1.11 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | MediumRun  | 15             | 2           | 10          |    13.48 ns |  0.063 ns |  0.093 ns |    13.50 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | MediumRun  | 15             | 2           | 10          |   773.22 ns | 13.525 ns | 18.960 ns |   758.22 ns |  1.00 |    0.02 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | MediumRun  | 15             | 2           | 10          | 1,457.39 ns |  7.419 ns | 10.640 ns | 1,457.20 ns |  1.88 |    0.02 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | MediumRun  | 15             | 2           | 10          | 1,462.93 ns |  2.769 ns |  3.972 ns | 1,462.29 ns |  1.89 |    0.01 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | MediumRun  | 15             | 2           | 10          |   775.49 ns |  3.674 ns |  5.385 ns |   772.02 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
