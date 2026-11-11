```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 537.4 ns |  10.88 ns | 7.20 ns |  1.39 |    0.02 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 376.4 ns |   1.78 ns | 0.93 ns |  0.97 |    0.00 | 0.0005 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 388.0 ns |   1.25 ns | 0.75 ns |  1.00 |    0.00 | 0.0005 |      40 B |        1.00 |
|                   |            |                |             |          |           |         |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 542.4 ns | 107.57 ns | 5.90 ns |  1.41 |    0.01 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 380.9 ns | 147.83 ns | 8.10 ns |  0.99 |    0.02 | 0.0005 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 385.4 ns |  38.42 ns | 2.11 ns |  1.00 |    0.01 | 0.0005 |      40 B |        1.00 |
