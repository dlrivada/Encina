```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.673 ns | 0.0308 ns | 0.0442 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 54.964 ns | 1.1291 ns | 1.6550 ns |  7.16 |    0.22 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 42.128 ns | 0.6121 ns | 0.8972 ns |  5.49 |    0.12 | 0.0091 |     152 B |        6.33 |
