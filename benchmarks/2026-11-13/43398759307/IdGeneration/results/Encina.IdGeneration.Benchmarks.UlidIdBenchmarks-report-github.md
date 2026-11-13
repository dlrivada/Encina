```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 542.0 ns |   5.33 ns | 3.52 ns |  1.13 |    0.02 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 464.3 ns |   8.42 ns | 5.57 ns |  0.97 |    0.02 | 0.0024 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 480.3 ns |  10.95 ns | 6.52 ns |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |         |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 575.4 ns | 176.87 ns | 9.69 ns |  1.16 |    0.02 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 470.4 ns |  31.87 ns | 1.75 ns |  0.95 |    0.01 | 0.0024 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 497.4 ns |  82.88 ns | 4.54 ns |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
