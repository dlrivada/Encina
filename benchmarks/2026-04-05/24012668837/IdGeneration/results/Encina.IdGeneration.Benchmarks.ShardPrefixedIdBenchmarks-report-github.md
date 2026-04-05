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
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.227 μs | 0.0051 μs | 0.0031 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.202 μs | 0.0182 μs | 0.0121 μs |  0.98 | 0.0057 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.420 μs | 0.0076 μs | 0.0050 μs |  1.16 | 0.0076 |     151 B |        1.26 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.254 μs | 0.0055 μs | 0.0036 μs |  1.02 | 0.0057 |     120 B |        1.00 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.398 μs | 0.0071 μs | 0.0047 μs |  1.14 | 0.0114 |     216 B |        1.80 |
|                                |            |                |             |             |              |             |               |           |           |       |        |           |             |
| Generate_UlidFormat            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,668.181 μs |        NA | 0.0000 μs |  1.00 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 19,791.260 μs |        NA | 0.0000 μs |  0.91 |      - |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,588.882 μs |        NA | 0.0000 μs |  1.00 |      - |     152 B |        1.27 |
| ExtractShardId_Ulid            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 24,693.728 μs |        NA | 0.0000 μs |  1.14 |      - |     120 B |        1.00 |
| Generate_ToString              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 22,412.672 μs |        NA | 0.0000 μs |  1.03 |      - |     216 B |        1.80 |
