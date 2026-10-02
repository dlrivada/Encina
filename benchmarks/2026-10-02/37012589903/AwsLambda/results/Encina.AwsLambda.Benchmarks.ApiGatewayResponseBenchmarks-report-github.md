```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     | 459.300 ns |   8.3150 ns |  7.7778 ns |  1.20 |    0.03 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |   9.213 ns |   0.1222 ns |  0.1083 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     | 406.730 ns |   3.3963 ns |  2.8360 ns |  1.06 |    0.02 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 869.365 ns |   5.5559 ns |  4.9251 ns |  2.27 |    0.04 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 793.507 ns |  14.5253 ns | 12.8763 ns |  2.07 |    0.05 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     | 383.352 ns |   7.3317 ns |  7.2007 ns |  1.00 |    0.03 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |            |             |            |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           | 448.500 ns | 151.3436 ns |  8.2957 ns |  1.17 |    0.02 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |   8.978 ns |   6.3420 ns |  0.3476 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           | 402.509 ns | 287.8496 ns | 15.7780 ns |  1.05 |    0.04 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 819.507 ns | 146.3922 ns |  8.0242 ns |  2.14 |    0.02 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 764.770 ns | 256.4461 ns | 14.0567 ns |  2.00 |    0.03 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           | 383.016 ns |  28.1804 ns |  1.5447 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
