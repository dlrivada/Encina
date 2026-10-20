```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 788.4 ns |  21.40 ns | 12.73 ns |  1.07 |    0.02 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 700.7 ns |  21.35 ns | 12.71 ns |  0.95 |    0.02 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 738.1 ns |  11.23 ns |  6.69 ns |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |          |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 799.9 ns | 294.18 ns | 16.13 ns |  1.08 |    0.02 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 705.8 ns | 242.19 ns | 13.28 ns |  0.95 |    0.02 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 741.0 ns | 223.68 ns | 12.26 ns |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |
