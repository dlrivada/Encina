```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 756.4 ns |  15.80 ns |  9.40 ns |  1.12 |    0.03 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 664.2 ns |  28.86 ns | 19.09 ns |  0.98 |    0.04 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 675.2 ns |  26.73 ns | 17.68 ns |  1.00 |    0.03 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |          |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 767.6 ns | 213.57 ns | 11.71 ns |  1.09 |    0.02 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 683.1 ns | 365.34 ns | 20.03 ns |  0.97 |    0.03 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 706.5 ns |  80.35 ns |  4.40 ns |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
