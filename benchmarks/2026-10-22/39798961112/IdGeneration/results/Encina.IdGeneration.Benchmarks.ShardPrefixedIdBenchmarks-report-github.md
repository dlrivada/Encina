```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 3           | 1.222 μs | 0.0155 μs | 0.0103 μs |  1.06 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 3           | 1.166 μs | 0.0039 μs | 0.0026 μs |  1.01 | 0.0076 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 3           | 1.155 μs | 0.0159 μs | 0.0095 μs |  1.00 | 0.0038 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 3           | 1.060 μs | 0.0075 μs | 0.0045 μs |  0.92 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 3           | 1.132 μs | 0.0041 μs | 0.0025 μs |  0.98 | 0.0038 |     120 B |        1.00 |
|                                |            |                |             |             |          |           |           |       |        |           |             |
| Generate_TimestampRandomFormat | MediumRun  | 15             | 2           | 10          | 1.256 μs | 0.0094 μs | 0.0134 μs |  1.16 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | MediumRun  | 15             | 2           | 10          | 1.166 μs | 0.0044 μs | 0.0064 μs |  1.08 | 0.0076 |     216 B |        1.80 |
| Generate_UlidFormat            | MediumRun  | 15             | 2           | 10          | 1.083 μs | 0.0024 μs | 0.0035 μs |  1.00 | 0.0038 |     120 B |        1.00 |
| Generate_UuidV7Format          | MediumRun  | 15             | 2           | 10          | 1.045 μs | 0.0019 μs | 0.0027 μs |  0.96 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | MediumRun  | 15             | 2           | 10          | 1.100 μs | 0.0022 μs | 0.0031 μs |  1.02 | 0.0038 |     120 B |        1.00 |
