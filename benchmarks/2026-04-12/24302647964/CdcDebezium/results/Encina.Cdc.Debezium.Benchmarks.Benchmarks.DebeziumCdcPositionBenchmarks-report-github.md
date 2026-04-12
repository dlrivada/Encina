```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.603 ns | 0.2522 ns | 0.3697 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 50.202 ns | 0.7686 ns | 1.1504 ns |  5.85 |    0.28 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 40.217 ns | 0.2130 ns | 0.3122 ns |  4.68 |    0.20 | 0.0091 |     152 B |        6.33 |
