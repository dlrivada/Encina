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
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           | 1.184 μs | 0.0041 μs | 0.0027 μs | 1.184 μs |  1.08 |    0.01 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           | 1.054 μs | 0.0058 μs | 0.0035 μs | 1.053 μs |  0.96 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 3           | 1.098 μs | 0.0096 μs | 0.0064 μs | 1.097 μs |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |          |           |           |          |       |         |        |           |             |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 1.184 μs | 0.0024 μs | 0.0036 μs | 1.183 μs |  1.07 |    0.01 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          | 1.064 μs | 0.0019 μs | 0.0028 μs | 1.064 μs |  0.96 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | MediumRun  | 15             | 2           | 10          | 1.110 μs | 0.0097 μs | 0.0139 μs | 1.119 μs |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |
