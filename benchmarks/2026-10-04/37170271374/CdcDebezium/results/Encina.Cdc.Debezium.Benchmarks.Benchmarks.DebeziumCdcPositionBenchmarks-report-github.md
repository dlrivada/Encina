```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.513 ns | 0.0258 ns | 0.0370 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 49.973 ns | 0.3325 ns | 0.4873 ns |  6.65 |    0.07 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 45.321 ns | 0.2966 ns | 0.4440 ns |  6.03 |    0.07 | 0.0091 |     152 B |        6.33 |
