```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.374 ns | 0.1179 ns | 0.1765 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 63.136 ns | 0.9233 ns | 1.2944 ns |  7.54 |    0.22 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 44.431 ns | 0.3585 ns | 0.5255 ns |  5.31 |    0.13 | 0.0091 |     152 B |        6.33 |
