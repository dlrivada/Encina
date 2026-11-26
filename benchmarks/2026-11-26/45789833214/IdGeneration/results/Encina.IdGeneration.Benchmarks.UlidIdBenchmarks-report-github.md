```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 563.8 ns |  23.29 ns | 13.86 ns |  1.37 |    0.04 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 395.9 ns |  15.97 ns | 10.57 ns |  0.97 |    0.03 | 0.0005 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 410.4 ns |  15.78 ns |  9.39 ns |  1.00 |    0.03 | 0.0005 |      40 B |        1.00 |
|                   |            |                |             |          |           |          |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 555.0 ns | 146.37 ns |  8.02 ns |  1.40 |    0.02 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 404.4 ns | 341.20 ns | 18.70 ns |  1.02 |    0.04 | 0.0005 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 397.4 ns |  19.28 ns |  1.06 ns |  1.00 |    0.00 | 0.0005 |      40 B |        1.00 |
