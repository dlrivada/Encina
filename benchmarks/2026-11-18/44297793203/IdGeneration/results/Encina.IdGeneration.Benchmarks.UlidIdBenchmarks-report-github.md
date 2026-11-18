```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 585.4 ns |   7.23 ns | 4.78 ns |  1.35 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 420.2 ns |   1.66 ns | 1.10 ns |  0.97 | 0.0005 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 432.6 ns |   1.76 ns | 1.16 ns |  1.00 | 0.0005 |      40 B |        1.00 |
|                   |            |                |             |          |           |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 590.0 ns | 126.46 ns | 6.93 ns |  1.39 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 418.6 ns |   6.64 ns | 0.36 ns |  0.99 | 0.0005 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 424.5 ns |   6.90 ns | 0.38 ns |  1.00 | 0.0005 |      40 B |        1.00 |
