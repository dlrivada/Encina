```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 3           | 1.135 μs | 0.0056 μs | 0.0033 μs | 1.134 μs |  1.13 |    0.00 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 3           | 1.157 μs | 0.0020 μs | 0.0013 μs | 1.157 μs |  1.15 |    0.00 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 3           | 1.009 μs | 0.0037 μs | 0.0022 μs | 1.009 μs |  1.00 |    0.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 3           | 1.034 μs | 0.0033 μs | 0.0017 μs | 1.034 μs |  1.02 |    0.00 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 3           | 1.026 μs | 0.0043 μs | 0.0028 μs | 1.025 μs |  1.02 |    0.00 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |             |          |           |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | MediumRun  | 15             | 2           | 10          | 1.160 μs | 0.0155 μs | 0.0206 μs | 1.149 μs |  1.15 |    0.02 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | MediumRun  | 15             | 2           | 10          | 1.100 μs | 0.0106 μs | 0.0141 μs | 1.095 μs |  1.09 |    0.02 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | MediumRun  | 15             | 2           | 10          | 1.010 μs | 0.0073 μs | 0.0099 μs | 1.003 μs |  1.00 |    0.01 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | MediumRun  | 15             | 2           | 10          | 1.003 μs | 0.0205 μs | 0.0280 μs | 1.024 μs |  0.99 |    0.03 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | MediumRun  | 15             | 2           | 10          | 1.040 μs | 0.0118 μs | 0.0161 μs | 1.031 μs |  1.03 |    0.02 | 0.0057 |     120 B |        1.00 |
