```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.56 ns |     1.512 ns |   0.083 ns |     1.26 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.94 ns |     3.864 ns |   0.212 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 50,900.24 ns | 4,929.050 ns | 270.178 ns | 2,320.00 |   22.05 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    101.92 ns |    12.559 ns |   0.688 ns |     4.65 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     66.57 ns |    12.808 ns |   0.702 ns |     3.03 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
