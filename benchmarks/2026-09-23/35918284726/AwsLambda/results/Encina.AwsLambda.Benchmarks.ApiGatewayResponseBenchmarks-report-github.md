```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   866.39 ns |  2.153 ns |  1.798 ns |  1.13 |    0.00 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    13.46 ns |  0.075 ns |  0.070 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   779.44 ns |  7.831 ns |  7.325 ns |  1.01 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,484.80 ns | 17.953 ns | 16.793 ns |  1.93 |    0.02 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,441.60 ns | 10.224 ns |  7.983 ns |  1.88 |    0.01 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   768.60 ns |  3.125 ns |  2.770 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |           |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   856.65 ns | 29.124 ns |  1.596 ns |  1.12 |    0.00 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    14.48 ns |  5.222 ns |  0.286 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   788.73 ns | 38.685 ns |  2.120 ns |  1.03 |    0.00 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,535.78 ns |  1.802 ns |  0.099 ns |  2.00 |    0.01 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,439.99 ns | 71.793 ns |  3.935 ns |  1.88 |    0.01 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   767.41 ns | 51.199 ns |  2.806 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
