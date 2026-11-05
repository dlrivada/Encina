```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 824.7 ns |  4.00 ns | 2.65 ns |  1.09 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 738.9 ns |  1.21 ns | 0.72 ns |  0.97 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 758.8 ns |  2.69 ns | 1.60 ns |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |          |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 839.2 ns | 80.25 ns | 4.40 ns |  1.05 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 738.7 ns | 65.79 ns | 3.61 ns |  0.92 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 799.6 ns | 65.12 ns | 3.57 ns |  1.00 | 0.0019 |      40 B |        1.00 |
