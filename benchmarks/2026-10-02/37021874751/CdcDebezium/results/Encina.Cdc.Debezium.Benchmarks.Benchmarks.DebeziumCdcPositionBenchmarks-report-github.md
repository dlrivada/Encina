```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.565 ns |  6.616 ns | 0.3627 ns |  1.00 |    0.06 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 43.335 ns | 12.571 ns | 0.6890 ns |  5.74 |    0.26 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 38.201 ns | 20.260 ns | 1.1105 ns |  5.06 |    0.25 | 0.0018 |     152 B |        6.33 |
