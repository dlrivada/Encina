```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   742.51 ns |   5.704 ns |  5.336 ns |  1.00 |    0.01 | 0.0191 |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,758.49 ns |  11.935 ns | 11.164 ns |  2.37 |    0.02 | 0.0477 |    1232 B |        2.48 |
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   838.14 ns |   9.030 ns |  8.446 ns |  1.13 |    0.01 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    15.69 ns |   0.083 ns |  0.078 ns |  0.02 |    0.00 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   814.20 ns |   2.106 ns |  1.970 ns |  1.10 |    0.01 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,804.16 ns |   6.620 ns |  6.192 ns |  2.43 |    0.02 | 0.0477 |    1232 B |        2.48 |
|                              |            |                |             |             |             |            |           |       |         |        |           |             |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   767.62 ns |  27.840 ns |  1.526 ns |  1.00 |    0.00 | 0.0191 |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,770.55 ns |  29.691 ns |  1.627 ns |  2.31 |    0.00 | 0.0477 |    1232 B |        2.48 |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   848.72 ns |  68.975 ns |  3.781 ns |  1.11 |    0.00 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    15.82 ns |   0.787 ns |  0.043 ns |  0.02 |    0.00 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   764.80 ns | 207.617 ns | 11.380 ns |  1.00 |    0.01 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,829.32 ns | 143.865 ns |  7.886 ns |  2.38 |    0.01 | 0.0477 |    1232 B |        2.48 |
