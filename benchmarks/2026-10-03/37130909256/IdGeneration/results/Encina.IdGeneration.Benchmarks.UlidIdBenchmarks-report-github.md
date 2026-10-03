```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 717.1 ns |    11.30 ns |  7.48 ns |  1.06 |    0.01 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 619.4 ns |    10.10 ns |  6.68 ns |  0.92 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 675.6 ns |     8.51 ns |  5.63 ns |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |             |          |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 798.8 ns | 1,658.38 ns | 90.90 ns |  1.17 |    0.11 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 674.1 ns |    28.86 ns |  1.58 ns |  0.98 |    0.00 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 685.2 ns |    50.10 ns |  2.75 ns |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
