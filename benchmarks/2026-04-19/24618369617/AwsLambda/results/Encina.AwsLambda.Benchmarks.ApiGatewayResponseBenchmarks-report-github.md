```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   842.58 ns | 13.183 ns | 11.687 ns |  1.19 |    0.02 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    18.67 ns |  0.460 ns |  1.355 ns |  0.03 |    0.00 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   732.41 ns |  9.515 ns |  8.901 ns |  1.04 |    0.02 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,746.90 ns |  7.466 ns |  6.618 ns |  2.47 |    0.03 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,763.72 ns | 17.164 ns | 16.055 ns |  2.49 |    0.03 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   707.61 ns |  7.780 ns |  7.277 ns |  1.00 |    0.01 | 0.0191 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |           |       |         |        |           |             |
| ToCreatedResponse_Success    | MediumRun  | 15             | 2           | 10          |   849.77 ns |  6.408 ns |  9.591 ns |  1.15 |    0.02 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | MediumRun  | 15             | 2           | 10          |    17.62 ns |  0.675 ns |  1.010 ns |  0.02 |    0.00 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | MediumRun  | 15             | 2           | 10          |   746.31 ns |  8.096 ns | 12.118 ns |  1.01 |    0.02 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | MediumRun  | 15             | 2           | 10          | 1,741.19 ns | 13.025 ns | 19.495 ns |  2.36 |    0.03 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | MediumRun  | 15             | 2           | 10          | 1,827.81 ns | 20.712 ns | 31.000 ns |  2.48 |    0.05 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | MediumRun  | 15             | 2           | 10          |   736.55 ns |  5.056 ns |  7.251 ns |  1.00 |    0.01 | 0.0191 |     496 B |        1.00 |
