```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 781.2 ns |  38.48 ns | 25.45 ns |  1.41 |    0.05 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 557.1 ns |  12.64 ns |  8.36 ns |  1.01 |    0.02 |      - |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 553.1 ns |   8.20 ns |  4.29 ns |  1.00 |    0.01 |      - |      40 B |        1.00 |
|                   |            |                |             |          |           |          |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 751.5 ns |  52.31 ns |  2.87 ns |  1.35 |    0.01 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 538.2 ns | 105.81 ns |  5.80 ns |  0.97 |    0.01 |      - |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 556.1 ns |  47.01 ns |  2.58 ns |  1.00 |    0.01 |      - |      40 B |        1.00 |
