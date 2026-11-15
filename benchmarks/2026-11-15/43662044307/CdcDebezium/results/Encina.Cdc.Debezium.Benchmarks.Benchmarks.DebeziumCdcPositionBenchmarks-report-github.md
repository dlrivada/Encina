```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.381 ns | 0.0785 ns | 0.1174 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 50.855 ns | 0.7248 ns | 1.0848 ns |  6.89 |    0.18 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 42.144 ns | 0.5480 ns | 0.7859 ns |  5.71 |    0.14 | 0.0091 |     152 B |        6.33 |
