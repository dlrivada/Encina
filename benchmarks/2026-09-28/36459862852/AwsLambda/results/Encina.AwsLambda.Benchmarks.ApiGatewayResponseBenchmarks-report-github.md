```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|---------:|------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   781.85 ns |  2.532 ns | 2.368 ns |  1.14 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    15.18 ns |  0.256 ns | 0.239 ns |  0.02 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   681.96 ns |  1.486 ns | 1.390 ns |  1.00 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,664.48 ns |  4.528 ns | 4.235 ns |  2.43 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,675.61 ns |  5.898 ns | 5.228 ns |  2.44 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   685.37 ns |  1.191 ns | 0.994 ns |  1.00 | 0.0191 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |          |       |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   800.73 ns | 23.951 ns | 1.313 ns |  1.17 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    15.81 ns |  6.881 ns | 0.377 ns |  0.02 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   692.58 ns | 24.038 ns | 1.318 ns |  1.01 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,741.25 ns | 82.034 ns | 4.497 ns |  2.55 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,824.27 ns | 42.242 ns | 2.315 ns |  2.67 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   683.27 ns |  6.565 ns | 0.360 ns |  1.00 | 0.0191 |     496 B |        1.00 |
