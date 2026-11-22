```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.815 ns | 0.0790 ns | 0.1158 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 50.758 ns | 2.2704 ns | 3.3982 ns |  6.50 |    0.44 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 35.956 ns | 0.5138 ns | 0.7531 ns |  4.60 |    0.12 | 0.0018 |     152 B |        6.33 |
