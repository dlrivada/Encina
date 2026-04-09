```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   824.34 ns |  1.715 ns |  1.339 ns |  1.08 |    0.01 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    16.07 ns |  0.045 ns |  0.037 ns |  0.02 |    0.00 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   727.17 ns |  2.716 ns |  2.540 ns |  0.95 |    0.01 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,819.38 ns |  4.533 ns |  4.018 ns |  2.38 |    0.02 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,796.85 ns |  5.525 ns |  5.168 ns |  2.35 |    0.02 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   763.37 ns |  5.787 ns |  5.413 ns |  1.00 |    0.01 | 0.0191 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |           |       |         |        |           |             |
| ToCreatedResponse_Success    | MediumRun  | 15             | 2           | 10          |   861.89 ns | 10.910 ns | 15.992 ns |  1.13 |    0.02 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | MediumRun  | 15             | 2           | 10          |    17.04 ns |  0.048 ns |  0.069 ns |  0.02 |    0.00 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | MediumRun  | 15             | 2           | 10          |   765.71 ns |  2.650 ns |  3.801 ns |  1.01 |    0.01 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | MediumRun  | 15             | 2           | 10          | 1,808.54 ns | 14.048 ns | 21.027 ns |  2.37 |    0.03 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | MediumRun  | 15             | 2           | 10          | 1,817.08 ns | 24.440 ns | 36.580 ns |  2.38 |    0.05 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | MediumRun  | 15             | 2           | 10          |   761.89 ns |  2.058 ns |  3.017 ns |  1.00 |    0.01 | 0.0191 |     496 B |        1.00 |
