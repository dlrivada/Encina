```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 580.8 ns |  7.61 ns | 4.53 ns |  1.37 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 415.3 ns |  0.71 ns | 0.47 ns |  0.98 | 0.0005 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 423.5 ns |  0.27 ns | 0.16 ns |  1.00 | 0.0005 |      40 B |        1.00 |
|                   |            |                |             |          |          |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 582.0 ns | 63.36 ns | 3.47 ns |  1.33 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 412.4 ns | 10.52 ns | 0.58 ns |  0.94 | 0.0005 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 438.0 ns |  4.53 ns | 0.25 ns |  1.00 | 0.0005 |      40 B |        1.00 |
