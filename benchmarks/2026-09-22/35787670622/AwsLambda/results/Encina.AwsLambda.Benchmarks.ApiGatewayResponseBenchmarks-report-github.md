```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.71GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|---------:|------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   782.79 ns |  1.693 ns | 1.414 ns |  1.15 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    15.61 ns |  0.137 ns | 0.128 ns |  0.02 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   678.49 ns |  1.259 ns | 1.116 ns |  1.00 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,663.65 ns |  3.035 ns | 2.534 ns |  2.45 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,784.96 ns |  2.018 ns | 1.685 ns |  2.63 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   678.07 ns |  1.266 ns | 0.988 ns |  1.00 | 0.0191 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |          |       |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   805.19 ns | 12.672 ns | 0.695 ns |  1.16 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    15.65 ns |  0.886 ns | 0.049 ns |  0.02 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   683.85 ns | 23.096 ns | 1.266 ns |  0.98 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,762.89 ns | 18.527 ns | 1.016 ns |  2.53 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,825.53 ns | 51.998 ns | 2.850 ns |  2.62 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   696.26 ns | 12.318 ns | 0.675 ns |  1.00 | 0.0191 |     496 B |        1.00 |
