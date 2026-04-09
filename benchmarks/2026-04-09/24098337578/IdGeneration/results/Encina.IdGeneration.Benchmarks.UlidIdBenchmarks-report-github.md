```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method            | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           | 1.329 μs | 0.0067 μs | 0.0040 μs | 1.330 μs |  1.15 |    0.00 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           | 1.153 μs | 0.0046 μs | 0.0028 μs | 1.152 μs |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 3           | 1.154 μs | 0.0066 μs | 0.0039 μs | 1.152 μs |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |          |           |           |          |       |         |        |           |             |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 1.304 μs | 0.0269 μs | 0.0394 μs | 1.337 μs |  1.09 |    0.05 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          | 1.133 μs | 0.0033 μs | 0.0047 μs | 1.134 μs |  0.95 |    0.03 | 0.0019 |      40 B |        1.00 |
| Generate          | MediumRun  | 15             | 2           | 10          | 1.198 μs | 0.0309 μs | 0.0433 μs | 1.235 μs |  1.00 |    0.05 | 0.0019 |      40 B |        1.00 |
