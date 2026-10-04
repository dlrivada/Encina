```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method            | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           | 1.256 μs | 0.0032 μs | 0.0021 μs | 1.256 μs |  1.06 |    0.00 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           | 1.059 μs | 0.0095 μs | 0.0056 μs | 1.060 μs |  0.89 |    0.00 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 3           | 1.189 μs | 0.0039 μs | 0.0023 μs | 1.189 μs |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |          |           |           |          |       |         |        |           |             |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 1.176 μs | 0.0026 μs | 0.0038 μs | 1.175 μs |  1.03 |    0.04 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          | 1.065 μs | 0.0026 μs | 0.0039 μs | 1.066 μs |  0.93 |    0.03 | 0.0019 |      40 B |        1.00 |
| Generate          | MediumRun  | 15             | 2           | 10          | 1.143 μs | 0.0287 μs | 0.0412 μs | 1.176 μs |  1.00 |    0.05 | 0.0019 |      40 B |        1.00 |
