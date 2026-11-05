```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.749 ns |  1.490 ns | 0.0816 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 53.320 ns | 12.324 ns | 0.6755 ns |  6.88 |    0.10 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 42.431 ns |  3.216 ns | 0.1763 ns |  5.48 |    0.05 | 0.0091 |     152 B |        6.33 |
