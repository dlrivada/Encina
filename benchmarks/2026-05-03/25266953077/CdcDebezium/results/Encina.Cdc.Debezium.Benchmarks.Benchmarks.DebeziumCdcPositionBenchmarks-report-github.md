```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.691 ns | 0.1386 ns | 0.2032 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 51.260 ns | 0.4057 ns | 0.5819 ns |  6.67 |    0.19 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 43.143 ns | 0.6969 ns | 1.0431 ns |  5.61 |    0.20 | 0.0091 |     152 B |        6.33 |
