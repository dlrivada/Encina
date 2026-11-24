```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 576.2 ns |   4.02 ns | 2.39 ns |  1.33 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 415.3 ns |   0.56 ns | 0.37 ns |  0.96 | 0.0005 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 431.6 ns |   0.57 ns | 0.38 ns |  1.00 | 0.0005 |      40 B |        1.00 |
|                   |            |                |             |          |           |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 600.4 ns | 117.68 ns | 6.45 ns |  1.42 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 414.2 ns |  10.04 ns | 0.55 ns |  0.98 | 0.0005 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 422.3 ns |   3.87 ns | 0.21 ns |  1.00 | 0.0005 |      40 B |        1.00 |
