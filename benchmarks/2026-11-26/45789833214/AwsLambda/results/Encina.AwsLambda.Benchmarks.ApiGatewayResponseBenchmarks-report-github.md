```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|---------:|------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   689.11 ns |  1.604 ns | 1.422 ns |  1.33 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    13.27 ns |  0.196 ns | 0.184 ns |  0.03 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   512.44 ns |  1.399 ns | 1.240 ns |  0.99 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,001.47 ns |  5.563 ns | 5.204 ns |  1.94 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     |   998.21 ns |  4.666 ns | 4.136 ns |  1.93 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   516.69 ns |  1.355 ns | 1.268 ns |  1.00 | 0.0057 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |          |       |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   712.92 ns |  1.420 ns | 0.078 ns |  1.35 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    12.85 ns |  2.967 ns | 0.163 ns |  0.02 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   515.88 ns | 16.735 ns | 0.917 ns |  0.98 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,022.08 ns |  8.961 ns | 0.491 ns |  1.93 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,023.91 ns | 44.116 ns | 2.418 ns |  1.94 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   528.86 ns | 19.891 ns | 1.090 ns |  1.00 | 0.0057 |     496 B |        1.00 |
