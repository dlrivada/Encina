```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.760 ns | 0.1388 ns | 0.1991 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 42.569 ns | 1.0114 ns | 1.5138 ns |  6.30 |    0.29 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 31.758 ns | 0.3314 ns | 0.4753 ns |  4.70 |    0.15 | 0.0091 |     152 B |        6.33 |
