```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   242.0 ns |   0.11 ns |  0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 1,114.8 ns |   2.54 ns |  1.33 ns |  4.61 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 1,106.5 ns |   7.70 ns |  5.10 ns |  4.57 |    0.02 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,380.1 ns |   4.30 ns |  2.85 ns |  5.70 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   613.6 ns |   3.49 ns |  2.08 ns |  2.54 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |            |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   242.0 ns |   1.90 ns |  0.10 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 1,118.4 ns | 201.16 ns | 11.03 ns |  4.62 |    0.04 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 1,191.8 ns |  17.07 ns |  0.94 ns |  4.93 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,371.0 ns |  92.02 ns |  5.04 ns |  5.67 |    0.02 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   609.9 ns |  14.16 ns |  0.78 ns |  2.52 |    0.00 |    2 |      - |         - |          NA |
