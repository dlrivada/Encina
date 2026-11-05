```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   242.0 ns |   0.09 ns |  0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     |   773.3 ns |   5.08 ns |  3.02 ns |  3.20 |    0.01 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     |   755.2 ns |   3.13 ns |  1.86 ns |  3.12 |    0.01 |    2 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,029.9 ns |   6.39 ns |  4.22 ns |  4.26 |    0.02 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   666.0 ns |   2.09 ns |  1.25 ns |  2.75 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |            |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   241.9 ns |   1.63 ns |  0.09 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           |   786.8 ns | 238.20 ns | 13.06 ns |  3.25 |    0.05 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           |   798.8 ns |  39.52 ns |  2.17 ns |  3.30 |    0.01 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,105.1 ns |  63.07 ns |  3.46 ns |  4.57 |    0.01 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   663.5 ns |   3.26 ns |  0.18 ns |  2.74 |    0.00 |    2 |      - |         - |          NA |
