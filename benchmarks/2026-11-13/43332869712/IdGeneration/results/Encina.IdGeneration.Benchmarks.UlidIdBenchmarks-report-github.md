```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 591.7 ns |  11.43 ns |  6.80 ns |  1.19 |    0.02 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 494.6 ns |  14.38 ns |  8.56 ns |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 495.8 ns |  13.41 ns |  8.87 ns |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |          |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 598.2 ns | 344.99 ns | 18.91 ns |  1.05 |    0.05 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 505.6 ns | 101.46 ns |  5.56 ns |  0.89 |    0.03 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 571.5 ns | 404.59 ns | 22.18 ns |  1.00 |    0.05 | 0.0019 |      40 B |        1.00 |
