```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.237 μs | 0.0030 μs | 0.0020 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.200 μs | 0.0041 μs | 0.0024 μs |  0.97 | 0.0057 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.351 μs | 0.0032 μs | 0.0021 μs |  1.09 | 0.0076 |     151 B |        1.26 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.261 μs | 0.0048 μs | 0.0029 μs |  1.02 | 0.0057 |     120 B |        1.00 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.403 μs | 0.0038 μs | 0.0022 μs |  1.13 | 0.0114 |     216 B |        1.80 |
|                                |            |                |             |             |              |             |               |           |           |       |        |           |             |
| Generate_UlidFormat            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 22,506.297 μs |        NA | 0.0000 μs |  1.00 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,831.794 μs |        NA | 0.0000 μs |  0.93 |      - |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,908.472 μs |        NA | 0.0000 μs |  0.97 |      - |     152 B |        1.27 |
| ExtractShardId_Ulid            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 26,037.557 μs |        NA | 0.0000 μs |  1.16 |      - |     120 B |        1.00 |
| Generate_ToString              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 25,280.796 μs |        NA | 0.0000 μs |  1.12 |      - |     216 B |        1.80 |
