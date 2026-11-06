```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 862.1 ns |  2.04 ns | 1.22 ns |  1.10 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 752.0 ns |  2.43 ns | 1.61 ns |  0.96 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 780.9 ns |  2.94 ns | 1.75 ns |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |          |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 851.5 ns | 15.64 ns | 0.86 ns |  1.02 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 759.2 ns | 46.05 ns | 2.52 ns |  0.91 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 831.0 ns | 21.47 ns | 1.18 ns |  1.00 | 0.0019 |      40 B |        1.00 |
