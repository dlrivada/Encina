```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   853.27 ns |   1.861 ns |  1.554 ns |  1.08 |    0.00 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    16.35 ns |   0.037 ns |  0.031 ns |  0.02 |    0.00 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   840.04 ns |  10.098 ns |  9.446 ns |  1.07 |    0.01 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,729.30 ns |   3.972 ns |  3.521 ns |  2.19 |    0.01 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,795.29 ns |   8.953 ns |  8.374 ns |  2.28 |    0.01 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   788.29 ns |   2.930 ns |  2.447 ns |  1.00 |    0.00 | 0.0191 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |           |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   883.64 ns |  69.391 ns |  3.804 ns |  1.08 |    0.02 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    16.57 ns |   2.430 ns |  0.133 ns |  0.02 |    0.00 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   740.82 ns |  52.175 ns |  2.860 ns |  0.91 |    0.01 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,751.92 ns |  54.322 ns |  2.978 ns |  2.14 |    0.03 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,866.64 ns | 121.198 ns |  6.643 ns |  2.28 |    0.03 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   817.83 ns | 244.991 ns | 13.429 ns |  1.00 |    0.02 | 0.0191 |     496 B |        1.00 |
