```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     21.84 ns |     10.539 ns |   0.578 ns |   1.23 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     17.76 ns |      2.230 ns |   0.122 ns |   1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 17,471.50 ns | 14,882.355 ns | 815.752 ns | 984.04 |   40.22 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     71.02 ns |     26.094 ns |   1.430 ns |   4.00 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     49.74 ns |     59.956 ns |   3.286 ns |   2.80 |    0.16 | 0.0105 |      - |     176 B |        1.57 |
