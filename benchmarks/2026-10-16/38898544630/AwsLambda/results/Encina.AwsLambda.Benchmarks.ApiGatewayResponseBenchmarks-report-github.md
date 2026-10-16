```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   862.73 ns |  17.023 ns | 17.482 ns |  1.35 |    0.03 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    15.20 ns |   0.219 ns |  0.205 ns |  0.02 |    0.00 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   626.23 ns |  10.203 ns |  9.544 ns |  0.98 |    0.02 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,297.51 ns |  25.783 ns | 22.856 ns |  2.03 |    0.04 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,311.84 ns |  25.210 ns | 23.582 ns |  2.05 |    0.04 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   639.13 ns |   7.287 ns |  6.816 ns |  1.00 |    0.01 | 0.0057 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |           |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   864.76 ns | 356.846 ns | 19.560 ns |  1.35 |    0.03 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    15.80 ns |   1.519 ns |  0.083 ns |  0.02 |    0.00 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   652.38 ns |  42.143 ns |  2.310 ns |  1.02 |    0.00 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,297.14 ns |  31.071 ns |  1.703 ns |  2.03 |    0.00 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,281.30 ns |  99.241 ns |  5.440 ns |  2.00 |    0.01 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   639.22 ns |  21.498 ns |  1.178 ns |  1.00 |    0.00 | 0.0057 |     496 B |        1.00 |
