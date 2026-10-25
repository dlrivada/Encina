```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method            | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           |   999.8 ns |  3.21 ns |  1.91 ns |   999.7 ns |  1.02 |    0.00 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           |   962.4 ns |  3.33 ns |  1.98 ns |   962.4 ns |  0.98 |    0.00 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 3           |   981.6 ns |  2.78 ns |  1.45 ns |   981.6 ns |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |            |          |          |            |       |         |        |           |             |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 1,056.6 ns |  2.88 ns |  4.04 ns | 1,056.6 ns |  1.10 |    0.04 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          |   930.1 ns | 21.98 ns | 30.08 ns |   930.1 ns |  0.97 |    0.05 | 0.0019 |      40 B |        1.00 |
| Generate          | MediumRun  | 15             | 2           | 10          |   959.9 ns | 24.38 ns | 34.96 ns |   986.0 ns |  1.00 |    0.05 | 0.0019 |      40 B |        1.00 |
