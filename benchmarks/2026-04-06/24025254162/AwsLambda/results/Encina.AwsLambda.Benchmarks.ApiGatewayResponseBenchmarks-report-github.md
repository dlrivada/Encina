```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   763.09 ns |   4.688 ns |  3.915 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,482.01 ns |  13.963 ns | 12.377 ns |  1.94 |    0.02 | 0.0725 |    1232 B |        2.48 |
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   880.38 ns |   2.321 ns |  1.938 ns |  1.15 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    14.23 ns |   0.258 ns |  0.241 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   778.61 ns |   5.154 ns |  4.569 ns |  1.02 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,459.19 ns |  10.176 ns |  9.021 ns |  1.91 |    0.01 | 0.0725 |    1232 B |        2.48 |
|                              |            |                |             |             |             |            |           |       |         |        |           |             |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   784.28 ns |  47.235 ns |  2.589 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,488.56 ns |  84.768 ns |  4.646 ns |  1.90 |    0.01 | 0.0725 |    1232 B |        2.48 |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   894.94 ns | 104.339 ns |  5.719 ns |  1.14 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    14.19 ns |   4.677 ns |  0.256 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   805.73 ns |  48.356 ns |  2.651 ns |  1.03 |    0.00 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,495.15 ns | 513.831 ns | 28.165 ns |  1.91 |    0.03 | 0.0725 |    1232 B |        2.48 |
