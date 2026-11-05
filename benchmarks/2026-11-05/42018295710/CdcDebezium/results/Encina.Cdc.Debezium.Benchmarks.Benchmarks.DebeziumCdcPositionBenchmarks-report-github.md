```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.068 ns | 1.1063 ns | 0.0606 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 41.886 ns | 0.7651 ns | 0.0419 ns |  6.90 |    0.06 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 34.681 ns | 1.3229 ns | 0.0725 ns |  5.72 |    0.05 | 0.0018 |     152 B |        6.33 |
