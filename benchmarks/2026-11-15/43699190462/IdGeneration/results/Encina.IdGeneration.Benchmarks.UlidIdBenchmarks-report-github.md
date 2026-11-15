```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 758.0 ns |   9.24 ns | 5.50 ns |  1.10 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 642.6 ns |   4.47 ns | 2.96 ns |  0.93 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 690.8 ns |   6.60 ns | 4.36 ns |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 729.6 ns | 126.52 ns | 6.94 ns |  1.09 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 687.4 ns | 133.64 ns | 7.33 ns |  1.03 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 669.8 ns |  91.55 ns | 5.02 ns |  1.00 | 0.0019 |      40 B |        1.00 |
