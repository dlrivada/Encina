```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.90GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.460 ns |  0.9813 ns | 0.0538 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 49.853 ns | 12.6662 ns | 0.6943 ns |  6.68 |    0.09 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 45.777 ns | 10.3929 ns | 0.5697 ns |  6.14 |    0.08 | 0.0091 |     152 B |        6.33 |
